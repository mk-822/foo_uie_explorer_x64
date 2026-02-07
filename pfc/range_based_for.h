#pragma once

/**
 * This file is essentially a patch to add some form of range-based for support to pfc containers
 * such as pfc::list_t<> and pfc::array_t<>.
 *
 * It's a self-contained file so that it can easily be removed if native support for range-based for
 * is added to pfc.
 * 
 * It must be explicitly included (it's not automatically included with pfc.h).
 */

namespace pfc {

namespace range_based_for_utils {

template <class Container>
class Iterator {
public:
	Iterator(Container& container, size_t initial_index = 0) : m_container{ container }, m_index{ initial_index } {}

	Iterator& operator++()
	{
		++m_index;
		return *this;
	}

	bool operator!=(Iterator& other) { return &other.m_container != &m_container || other.m_index != m_index; }

	decltype(std::declval<Container&>()[0]) operator*() { return m_container[m_index]; }

private:
	Container& m_container;
	size_t m_index{};
};

template <class Container>
auto begin_generic(Container&& container)
{
	return Iterator<decltype(container)>(container);
}

template <class Container>
auto end_generic(Container&& container)
{
	return Iterator<decltype(container)>(container, pfc::array_size_t(container));
}

}

template <class Element>
auto begin(pfc::array_t<Element>& list)
{
	return range_based_for_utils::begin_generic(list);
}

template <class Element>
auto end(pfc::array_t<Element>& list)
{
	return range_based_for_utils::end_generic(list);
}

template <class Element>
auto begin(const pfc::list_base_const_t<Element>& list)
{
	return range_based_for_utils::begin_generic(list);
}

template <class Element>
auto end(const pfc::list_base_const_t<Element>& list)
{
	return range_based_for_utils::end_generic(list);
}

template <class Element>
auto begin(pfc::list_base_t<Element>& list)
{
	return range_based_for_utils::begin_generic(list);
}

template <class Element>
auto end(pfc::list_base_t<Element>& list)
{
	return range_based_for_utils::end_generic(list);
}

}
